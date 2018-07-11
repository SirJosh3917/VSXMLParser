# VSXMLParser
Parse VS's XML documentation into markdown

## Output example
[See the output generated for this project!](EXAMPLE.md)

## How do I use it
Drag and drop the XML file you want to turn into markdown onto the EXE. Remember, for the parameter names to show up in the correct order, they have to be listed as such - the first parameter going to the first param name.

For example, this is correct:

```
	/// <param name="a">'a' just so happens to be corresponding to T1</param>
	/// <param name="b">And b just so happens to be corresponding to T2</param>
	public T SomeGenericFunction<T1, T2>(T1 a, T2 b) {
		return default(T);
	}
```

Anything else is incorrect:

```
	/// <param name="b">And b just so happens to be corresponding to T2</param>
	/// <param name="a">'a' just so happens to be corresponding to T1</param>
	public T SomeGenericFunction<T1, T2>(T1 a, T2 b) {
		
	/// <param name="b">And b just so happens to be corresponding to T2</param>
	public T SomeGenericFunction<T1, T2>(T1 a, T2 b) {
		
	// this one is actually correct because 'a' corresponds to the first parameter, with no parameter listed for 'b'
	/// <param name="a">'a' just so happens to be corresponding to T1</param>
	public T SomeGenericFunction<T1, T2>(T1 a, T2 b) {
```